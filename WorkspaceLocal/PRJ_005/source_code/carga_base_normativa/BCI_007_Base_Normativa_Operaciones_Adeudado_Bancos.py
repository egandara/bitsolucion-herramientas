# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_007_Base_Normativa_Operaciones_Adeudado_Bancos
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ***************************************************************************************************************
# MAGIC * Nombre: BCI_007_Base_Normativa_Adeudado_Bancos.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones adeudadas por bancos
# MAGIC * Documentación: 
# MAGIC ***************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * slv_Parametricas_db.TBU
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos (Activo 1 Tabla 89 Manual CMF)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci07_basenorma_tmp_adeudado_bancos

# COMMAND ----------

drop_adeudado_bancos = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci07_basenorma_tmp_adeudado_bancos"""

# COMMAND ----------

sql_safe(drop_adeudado_bancos)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci07_basenorma_tmp_adeudado_bancos", True)

# COMMAND ----------

create_adeudado_bancos = """CREATE TABLE """ + db_plat_tempX + """.bci07_basenorma_tmp_adeudado_bancos
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  c11_tipo_activo               	INT,
  c11_nombre_activo             	STRING,
  exposicion                    	DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  pvtg                      		  DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci07_basenorma_tmp_adeudado_bancos' """

# COMMAND ----------

sql_safe(create_adeudado_bancos)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci07_basenorma_tmp_adeu_banc_pi_pdi_pe

# COMMAND ----------

drop_adeu_banc_pi_pdi_pe = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci07_basenorma_tmp_adeu_banc_pi_pdi_pe"""

# COMMAND ----------

sql_safe(drop_adeu_banc_pi_pdi_pe)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci07_basenorma_tmp_adeu_banc_pi_pdi_pe", True)

# COMMAND ----------

create_adeu_banc_pi_pdi_pe = """CREATE TABLE """ + db_plat_tempX + """.bci07_basenorma_tmp_adeu_banc_pi_pdi_pe
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  c11_tipo_activo               	INT,
  c11_nombre_activo             	STRING,
  pi					                    DECIMAL(30, 6),
  pdi					                    DECIMAL(30, 6),
  pe					                    DECIMAL(30, 7),
  provision			                  DECIMAL(30, 0),
  calificacion_cli		            STRING,
  exposicion                    	DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pvtg                      		  DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci07_basenorma_tmp_adeu_banc_pi_pdi_pe' """

# COMMAND ----------

sql_safe(create_adeu_banc_pi_pdi_pe)

# COMMAND ----------

# MAGIC %md
# MAGIC ####Tabla tmp_modelo_ind_agrp

# COMMAND ----------

drop_tmp_modelo_ind_agrp = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_ind_agrp"""

# COMMAND ----------

sql_safe(drop_tmp_modelo_ind_agrp)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_ind_agrp", True)

# COMMAND ----------

create_tmp_modelo_ind_agrp = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_ind_agrp
(
  periodo_id			      INT,
  rut_cliente				    INT,
  dv_cliente            STRING,
  cod_num_operacion		  STRING,	
  cod_tipo_Activo				STRING,
  pct_pi					      DECIMAL(30, 6),
  pct_pdi					      DECIMAL(30, 6),
  pct_pe					      DECIMAL(30, 6),
  mnt_provision			    DECIMAL(30, 6),
  cod_calificacion		  STRING,
  cod_tipo_garantia		  STRING,
  mnt_garantia		      DECIMAL(30, 6)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_ind_agrp' """
 

# COMMAND ----------

sql_safe(create_tmp_modelo_ind_agrp)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Ultimo dá habil mes de fecha de Proceso

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 2: Obtencion Univ Operaciones Adeudadas Bancos

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones adeudados por bancos desde tmp_d00_deuda_act_ctg y TBU
# MAGIC
# MAGIC **Adeudado por Banco (Cuentas IFRS 143)**
# MAGIC - **Comienzan en 1431**:	Bancos del País
# MAGIC - **Comienzan en 1432**:	Bancos del Exterior
# MAGIC - **Comienzan en 1433**:	Bancos Central de Chile
# MAGIC - **Comienzan en 1434**:	Bancos Centrales del Exterior
# MAGIC
# MAGIC **Tabla de salida**: bci07_basenorma_tmp_adeudado_bancos (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **c11_tipo_activo**: Identificador del tipo activos C11
# MAGIC - **c11_nombre_activo**: Nombre del tipo activo C11
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

diaHabil = spark.sql("""select max(fecha_informada) as periodo_id  FROM  """+ db_slv_ParametricasX +""".TBU  
where substring(fecha_informada, 1, 6) = """+ anomes +"""  """)

ultimoDiaHabil_tbu = diaHabil.toPandas()
dia_tbu = ultimoDiaHabil_tbu.iloc[0]["periodo_id"]
 
print(dia_tbu)

# COMMAND ----------

vista_adeudado_bancos = """ insert into """ + db_plat_tempX + """.bci07_basenorma_tmp_adeudado_bancos
  select  a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          1                               as c11_tipo_activo,
          'ADEUDADO POR BANCOS'           as c11_nombre_activo,
          0                               as exposicion,
          0.0                             as factor_expo,
          0.0                             as pvtg,
          0.0                             as pi_metodo_interno,
          0.0                             as pdi_metodo_interno,
          0.0                             as pe_metodo_interno,
          0.0                             as prov_metodo_interno,
          0.0                             as monto_avalado				
  from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
  inner join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = substring(b.fecha_informada, 1, 6)
  where a.periodo_id = '""" + anomes + """' 
    and trim(a.cod_cartera_ope) = 'ACT'
    and substring(b.cta_cmf, 1, 3) = '143'
    and b.fecha_informada = '""" + str(dia_tbu) + """'
"""

# COMMAND ----------

sql_safe(vista_adeudado_bancos)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Obtencion Operaciones Modelo Individual, Agrupadas por Calificacion, Pi, PDI y PE

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla tmp_modelo_indiv generada en el Notebook 02
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_ind_agrp (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMM
# MAGIC - **rut_cliente**: Rut del cliente
# MAGIC - **dv_cliente**: digito verificador del cliente
# MAGIC - **cod_num_operacion**: Codigo operacion
# MAGIC - **cod_tipo_activo**: Tipo Activo ACT/CTG
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada	
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calificacion**: Codigo clasificacion 
# MAGIC - **cod_tipo_garantia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia

# COMMAND ----------

query_ins_mod_ind = """ INSERT INTO """ + db_plat_tempX + """.tmp_modelo_ind_agrp
select distinct a.periodo_id
      ,a.rut_cliente	
      ,a.dv_cliente 				
      ,a.cod_num_operacion
      ,(CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END) as cod_tipo_Activo
      ,a.pct_pi                            
      ,a.pct_pdi                          
      ,a.pct_pe                            
      ,sum(a.mnt_provision) mnt_provision
      ,a.cod_calificacion                  
      ,a.cod_tipo_garantia                  
      ,sum(a.mnt_garantia) as mnt_garantia
from """ + db_plat_tempX + """.tmp_modelo_indiv a 
 where a.periodo_id= """+anomes+"""
 group by a.periodo_id
      ,a.rut_cliente	
      ,a.dv_cliente 				
      ,a.cod_num_operacion
      ,(CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END)  
      ,a.pct_pi                            
      ,a.pct_pdi                          
      ,a.pct_pe                                  
      ,a.cod_calificacion                  
      ,a.cod_tipo_garantia
"""

# COMMAND ----------

sql_safe(query_ins_mod_ind)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 4: Obtencion PI, PDI y PE Operaciones Adeudadas Bancos

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de la PI, PDI y PE de cada operaciones desde bci07_basenorma_tmp_adeudado_bancos y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci07_basenorma_tmp_adeu_banc_pi_pdi_pe (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **c11_tipo_activo**: Identificador del tipo activos C11
# MAGIC - **c11_nombre_activo**: Nombre del tipo activo C11
# MAGIC - **pi**: Porcentaje de la probabilidad incumplimiento individual
# MAGIC - **pdi**: Porcentaje de perdida dada por el incumplimiento individual
# MAGIC - **pe**: Porcentaje de perdida esperada individual
# MAGIC - **provision**: Monto provisional individual
# MAGIC - **calificacion_cli**: Codigo clasificacion individual
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Tiene o no garantia
# MAGIC - **monto_garantia**: Monto de la garantia
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_adeu_banc_pi_pdi_pe = """ insert into """ + db_plat_tempX + """.bci07_basenorma_tmp_adeu_banc_pi_pdi_pe
select distinct a.periodo_id
      ,a.rut_cliente	
      ,a.dv_cliente 				
      ,a.cod_ope_original
      ,a.cod_num_operacion
      ,a.cod_tip_cart		
      ,a.cod_tipo_ope
      ,a.des_producto_ope
      ,a.des_banca_ope			
      ,a.cod_cartera_ope
      ,a.mto_deuda_ope   			
      ,a.num_dias_mora_ope
      ,a.ind_cdet	
      ,a.fec_ingreso_deteriodo_ope
      ,a.cod_cctb
      ,a.fl_ope_reneg      
      ,a.c11_tipo_activo
      ,a.c11_nombre_activo
      ,b.pct_pi                            as pi
      ,b.pct_pdi                           as pdi
      ,b.pct_pe                            as pe
      ,b.mnt_provision                     as provision
      ,b.cod_calificacion                  as calificacion_cli
      ,a.exposicion
      ,a.factor_expo
      ,b.cod_tipo_garantia                 as tipo_garantia
      ,b.mnt_garantia                      as monto_garantia
      ,a.pvtg
      ,a.pi_metodo_interno
      ,a.pdi_metodo_interno
      ,a.pe_metodo_interno
      ,a.prov_metodo_interno
      ,a.monto_avalado	
from """ + db_plat_tempX + """.bci07_basenorma_tmp_adeudado_bancos a
left join """ + db_plat_tempX + """.tmp_modelo_ind_agrp b
    on a.periodo_id = b.periodo_id
    and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and trim(a.cod_cartera_ope) = (b.cod_tipo_activo)
 where a.periodo_id= """ + anomes + """
"""

# COMMAND ----------

sql_safe(vista_adeu_banc_pi_pdi_pe)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 5: Eliminacion Adeudados Bando para Fecha de Proceso 

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v3_ft correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)

# COMMAND ----------

delete_base_normativa_v3_ft = """delete from """ + db_platinumX + """.base_archivos_normativos where periodo_id = """+anomes+ """ and cod_tabla_89 = 1 """

# COMMAND ----------

sql_safe(delete_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 6: Insercion Datos Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v3_ft desde la vista bci07_basenorma_tmp_adeu_banc_pi_pdi_pe
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Codigo tipo activo
# MAGIC - **nom_activo**: Nombre del tipo activo
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calif**: Codigo tipo clasificacion cliente
# MAGIC - **mnt_provision**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **des_tipo_gtia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **pct_ltv**: Porcentaje PVTG
# MAGIC - **pct_pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pct_pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pct_pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **mnt_prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **mnt_avalado**: Monto avalado del cliente	

# COMMAND ----------

insert_base_normativa_v3_ft = """ insert into """ + db_platinumX + """.base_archivos_normativos
  select  periodo_id, 				
          rut_cliente, 				
          dv_cliente, 				
          cod_ope_original, 			
          cod_num_operacion, 		
          cod_tip_cart, 				
          cod_tipo_ope, 				
          des_producto_ope, 			
          des_banca_ope, 			
          cod_cartera_ope,			
          mto_deuda_ope,	   			
          num_dias_mora_ope,		
          ind_cdet, 					
          fec_ingreso_deteriodo_ope, 
          cod_cctb, 					
          fl_ope_reneg,
          -998                             as cod_tabla_34,
          c11_tipo_activo					          as cod_tipo_activo,
          c11_nombre_activo				          as nom_activo,
          pi								                as pct_pi,
          pdi								                as pct_pdi,
          pe								                as pct_pe,
          provision						              as mnt_provision,
          calificacion_cli					        as cod_calif,
          exposicion						            as mnt_provision,
          factor_expo,
          tipo_garantia                     as des_tipo_gtia,
          monto_garantia                    as mnt_garantia,
          pvtg							                as pct_ltv,
          pi_metodo_interno				          as pct_pi_metodo_interno,
          pdi_metodo_interno				        as pct_pdi_metodo_interno,
          pe_metodo_interno				          as pct_pe_metodo_interno,
          prov_metodo_interno				        as mnt_prov_metodo_interno,
          monto_avalado					            as mnt_avalado,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci07_basenorma_tmp_adeu_banc_pi_pdi_pe
"""

# COMMAND ----------

sql_safe(insert_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_platinumX+""".base_archivos_normativos  where periodo_id = """+anomes+""" and cod_tabla_89 = 1 """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_colcomer ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")