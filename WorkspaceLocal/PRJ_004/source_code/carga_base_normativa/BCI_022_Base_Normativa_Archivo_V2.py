# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_022_Base_Normativa_Archivo_V2
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************************************************************************
# MAGIC * Nombre: BCI_021_Base_Normativa_Operaciones_C12_Prorr_C12_Reg2_C19_C20_C50.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones Activas C12 Prorrogas, C12 Registro 2, C19, C20 y C50
# MAGIC * Documentación: 
# MAGIC **************************************************************************************************************************************

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
# MAGIC * tmp_modelo_com_gr
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos

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
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ####Tabla Temporal V3

# COMMAND ----------

drop_tmp_base_normativa_v3 = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_base_normativa_v3"""

# COMMAND ----------

sql_safe(drop_tmp_base_normativa_v3)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_base_normativa_v3", True)

# COMMAND ----------

create_tmp_base_normativa_v3 = """CREATE TABLE IF NOT EXISTS """ + db_plat_tempX + """.tmp_base_normativa_v3
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera (OPE_VIGENTE) ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mnt_deuda_ope	   DECIMAL(32,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deterioro_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada',
  cod_tabla_34  INT COMMENT 'Tipo Activo Créditos de consumo y de vivienda Tabla CMF 34',
  cod_tabla_89  INT COMMENT 'Tipos de activos y créditos contingentes Tabla CMF 89',  
	nom_activo STRING COMMENT 'Nombre Activo Cartera',
	pct_pi DECIMAL(12,6) COMMENT 'PI',
  pct_pdi DECIMAL(12,6) COMMENT 'PDI',
  pct_pe DECIMAL(12,6) COMMENT 'PE',
	mnt_provision DECIMAL(32,0) COMMENT 'Prov Oficial',
	cod_calif STRING COMMENT 'Calificacion Cliente GR/A1/B1/C4',
	mnt_exposicion DECIMAL(32,0) COMMENT 'Prov Oficial',
	factor_expo DECIMAL(12,6) COMMENT 'PI',
	des_tipo_gtia STRING COMMENT 'Tipo garantia',		
	mnt_garantia DECIMAL(32,0) COMMENT 'MONTO_GARANTIA' ,
	pct_ltv DECIMAL(12,6) COMMENT 'PTVG',
	pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
	mnt_prov_metodo_interno  DECIMAL(32,0) COMMENT 'Provisión de la Colocación',
	mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO',
  fec_proceso DATE COMMENT 'Fecha de cierre' 
) 
USING DELTA
PARTITIONED BY(fec_proceso)
COMMENT 'Tabla que permite almacenar información del modelo normativo V3
Actualizacion: Mensual Incremental'
LOCATION '""" + db_location_plat_tempX + """tmp_base_normativa_v3'  """

# COMMAND ----------

sql_safe(create_tmp_base_normativa_v3)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 1: Obtencion Operaciones base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se realiza la actualizacion del campo cod_tabla_34 en la tabla base_normativa_v3_ft
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **cod_tabla_34**: Tipo Activo Créditos de consumo y de vivienda Tabla CMF 34

# COMMAND ----------

query_ins_temp_v3 = """ INSERT INTO """ + db_plat_tempX + """.tmp_base_normativa_v3
select periodo_id
  ,rut_cliente
  ,dv_cliente
  ,cod_ope_original
  ,cod_num_operacion
  ,cod_tip_cart
  ,cod_tipo_ope
  ,des_producto_ope
  ,des_banca_ope
  ,cod_cartera_ope
  ,mnt_deuda_ope
  ,num_dias_mora_ope
  ,ind_cdet
  ,fec_ingreso_deterioro_ope
  ,cod_cctb
  ,fl_ope_reneg
  ,cod_tabla_34
  ,cod_tabla_89
  ,nom_activo
  ,pct_pi
  ,pct_pdi
  ,pct_pe
  ,mnt_provision
  ,cod_calif
  ,mnt_exposicion
  ,factor_expo
  ,des_tipo_gtia
  ,mnt_garantia
  ,pct_ltv
  ,pct_pi_metodo_interno
  ,pct_pdi_metodo_interno
  ,pct_pe_metodo_interno
  ,mnt_prov_metodo_interno
  ,mnt_avalado
  ,fec_proceso
from """+db_platinumX+""".base_archivos_normativos
where periodo_id = """+anomes+"""
 """

sql_safe(query_ins_temp_v3)

# COMMAND ----------

# MAGIC %md
# MAGIC ####Paso 2: Obtencion Archivos Normativos V2

# COMMAND ----------

query_arch_bn_v2 = """ create or replace temporary view vw_archivo_base_normativa_v2 as
select distinct periodo_id
  ,rut_cliente
  ,dv_cliente
  ,cod_num_operacion
  ,cod_activo
  ,case when int(num_registro) = 1 then 'ACT'
        when num_registro = 2 then 'CTG'
  else '' end as tipo_activo
  ,cod_cta_contable
  ,cod_ope_original
from """+db_plat_tempX+""".base_normativa_v2_ft
where periodo_id = """+anomes+"""
"""


sql_safe(query_arch_bn_v2)

# COMMAND ----------

# MAGIC %md
# MAGIC ####Paso 3 :Eliminacion Registros V3 para Fecha Proceso

# COMMAND ----------

query_delete_v3 = """ DELETE FROM """+db_platinumX+""".base_archivos_normativos
where periodo_id = """+anomes+"""
 """ 

sql_safe(query_delete_v3)

# COMMAND ----------

# MAGIC %md
# MAGIC ####Paso 4: Insert V3 Actualizado con Activo Tabla 34

# COMMAND ----------

query_insert_v3 = """ INSERT INTO """+db_platinumX+""".base_archivos_normativos
select a.periodo_id
  ,a.rut_cliente
  ,a.dv_cliente
  ,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart
  ,a.cod_tipo_ope
  ,a.des_producto_ope
  ,a.des_banca_ope
  ,a.cod_cartera_ope
  ,a.mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deterioro_ope
  ,a.cod_cctb
  ,a.fl_ope_reneg
  ,coalesce(b.cod_activo, a.cod_tabla_34) as cod_tabla_34
  ,a.cod_tabla_89
  ,a.nom_activo
  ,a.pct_pi
  ,a.pct_pdi
  ,a.pct_pe
  ,a.mnt_provision
  ,a.cod_calif
  ,a.mnt_exposicion
  ,a.factor_expo
  ,a.des_tipo_gtia
  ,a.mnt_garantia
  ,a.pct_ltv
  ,a.pct_pi_metodo_interno
  ,a.pct_pdi_metodo_interno
  ,a.pct_pe_metodo_interno
  ,a.mnt_prov_metodo_interno
  ,a.mnt_avalado
  ,a.fec_proceso
from """+db_plat_tempX+""".tmp_base_normativa_v3 a
full join vw_archivo_base_normativa_v2 b
 on a.periodo_id = b.periodo_id
 and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
 and a.rut_cliente = b.rut_cliente
 and trim(a.cod_cartera_ope) = trim(b.tipo_activo)
 --and trim(a.cod_cctb) = trim(b.cod_cta_contable)
 --and trim(a.cod_ope_original) = trim(b.cod_ope_original)
where a.periodo_id = """+anomes+"""
  and a.cod_num_operacion is not null
 """

sql_safe(query_insert_v3)

# COMMAND ----------

# MAGIC %md
# MAGIC ####Paso 5 :Eliminacion Registros V3 para Periodo NULA

# COMMAND ----------

query_delete_v3_null = """ DELETE FROM """+db_platinumX+""".base_archivos_normativos
where periodo_id is null
 """ 

sql_safe(query_delete_v3_null)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Conteo de registros insertados en tabla de salida

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla base_normativa_v3_ft Actualizacion

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """ +anomes + """ """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Actualizaron " + str(cantidad) +" Registros en la tabla base_archivos_normativos")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")