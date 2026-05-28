# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_004_Base_Normativa_Modelo_Hipotecario_Vivienda

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_004_Base_Normativa_Modelo_Hipotecario_Vivienda.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: Jonathan Araya R. BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de registrar información de Operaciones Hipotecarias de Vivienda de Modelo B1 y Modelo Interno
# MAGIC * Documentación: 
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tabla Entrada ADLS: 
# MAGIC * slv_RiesgoCred_RiesgoCredPer_db.result_pi_hip
# MAGIC * prv_hip.prv_viv_ft_interno_bci
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * tmp_modelo_hip
# MAGIC * tmp_modelo_hip_estandar

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_plat_tempW","","02 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","03 Location platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_provHipotecarioW","","05 ProvisionHipotecario:")
dbutils.widgets.text("db_slv_ParametricasW","","06 Parametricas DB:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_provHipotecarioX = dbutils.widgets.get("db_provHipotecarioW")
spark.conf.set("bci.db_provHipotecarioX", db_provHipotecarioX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_provHipotecarioX: " + db_provHipotecarioX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
#valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

#valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ## Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion tablas temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_modelo_hip

# COMMAND ----------

paso_tb_del_tmp_modelo_hip  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_hip"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_modelo_hip)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_hip", True)

# COMMAND ----------

paso_tb_crea_tmp_modelo_hip = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_hip
(
	periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'rut cliente',
  dv_cliente STRING COMMENT 'dígito verificador cliente',
  cod_num_operacion STRING COMMENT 'Codigo numero operación',
  pct_pi DECIMAL(12,6) COMMENT 'Probabilidad Incumplimiento',
  pct_pdi DECIMAL(12,6) COMMENT 'Pérdida dado el incumplimiento del cliente (PDI)',
  pct_pe DECIMAL(12,6) COMMENT 'Pérdida Esperada',
  mnt_operacion  BIGINT COMMENT 'Saldo IFRS',
  mnt_provision DECIMAL(32,6)  COMMENT 'Provisión estimada considerando pdi_rc',
  pct_ltv DECIMAL(20,6) COMMENT 'relación entre la deuda actual y el valor del bien al origen'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_hip' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_modelo_hip)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_modelo_hip_estandar

# COMMAND ----------

paso_tb_del_tmp_modelo_hip_b1  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_hip_estandar"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_modelo_hip_b1)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_hip_estandar", True)

# COMMAND ----------

paso_tb_crea_tmp_modelo_hip_b1 = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_hip_estandar
(
	periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
	rut_cliente INT COMMENT 'rut cliente',
	dv_cliente STRING COMMENT 'dígito verificador cliente',
	cod_num_operacion STRING COMMENT 'Número de la operación HIP',
	pct_pi DECIMAL(12,6) COMMENT 'Probabilidad Incumplimiento',
	pct_pdi DECIMAL(12,6) COMMENT 'Pérdida dado el incumplimiento del cliente (PDI)',
	pct_pe DECIMAL(12,6) COMMENT 'Pérdida Esperada',
	mnt_operacion  DECIMAL(20,6) COMMENT 'EAD o Total saldo IFRS',
	mnt_provision DECIMAL(32,6)  COMMENT 'Provisión',
	pct_ltv DECIMAL(20,6) COMMENT 'Prestamo versus Garantía (Loan-to-value)'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_hip_estandar' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_modelo_hip_b1)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 1:  Obtención último día hábil del mes

# COMMAND ----------

diaHabil = spark.sql("""select max(cal_ano || string(LPAD(TRIM(cast(cal_mes as char(11))),2,'0')) ||  string(LPAD(TRIM(cast(cal_dia as char(11))),2,'0'))) as periodo_id  FROM  """+db_slv_ParametricasX +""".bci_cal_dia  
where cal_ano || string(LPAD(TRIM(cast(cal_mes as char(11))),2,'0')) = """+ anomes +"""
and TRIM(cal_ind_dia) = 'H'""")

ultimoDiaHabil = diaHabil.toPandas()
dia = ultimoDiaHabil.iloc[0]["periodo_id"]
print(dia) 

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 2: Obtencion fecha provisiones Hipotecarias

# COMMAND ----------

periodo_prov_provope_hip = spark.sql("""select max(fechaproceso) fecha_proceso from """+db_RiesgoCredX +""".tbl_prov_provope_hip where fecha_informada = """+str(dia)+""" """)

periodo_provisiones_hip = periodo_prov_provope_hip.toPandas()
fecha_prov = periodo_provisiones_hip.iloc[0]["fecha_proceso"]
print(fecha_prov)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Cartera Hipotecaria Interna

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones hipotecarias agragando Loan To Value Oficial desde prv_hip.prv_viv_ft_interno_bci(TABPROV_PROVOPE_INTERNO) y slv_RiesgoCred_RiesgoCredPer_db.result_pi_hip (MODELO_HIPOTECARIO_B1)
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_hip(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **pct_pi**: Probabilidad Incumplimiento
# MAGIC - **pct_pdi** : Pérdida dado el incumplimiento del cliente (PDI)
# MAGIC - **pct_pe**: Pérdida Esperada
# MAGIC - **mnt_operacion**: Saldo IFRS
# MAGIC - **mnt_provision**: Provisión estimada
# MAGIC - **pct_ltv**: relación entre la deuda actual y el valor del bien al origen

# COMMAND ----------

query_ins_hip_interno = """ insert into """+db_plat_tempX+""".tmp_modelo_hip  
select """+anomes+"""        			as periodo_id	
			,mode_int.rut        				as rut_cliente
      ,mode_int.digitoverificador as dv_cliente	
   		,mode_int.operacion			    as cod_num_operacion	
			,mode_int.pi	          		as pct_pi
			,mode_int.pdi	          		as pct_pdi
			,mode_int.pe            		as pct_pe	
   		,mode_int.monto_operacion		as monto_operacion			
   		,mode_int.provision 				as provision
			,mode_b1.PVG                as ltv 
from """+db_RiesgoCredX+""".tbl_prov_provope_hip mode_int
left join """+db_RiesgoCredX+""".result_pi_hip mode_b1
  on substring(mode_int.fecha_informada, 1, 6) = mode_b1.anomes
  and trim(mode_int.operacion) = trim(mode_b1.numero_operacion)
where mode_int.fecha_informada = """+str(dia)+"""
    and mode_int.fechaproceso = '"""+str(fecha_prov)+"""'
"""

# COMMAND ----------

sql_safe(query_ins_hip_interno)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Obtencion Cartera Hipotecario modelo estandar

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de operaciones hipotecarias vivienda b1 desde slv_RiesgoCred_RiesgoCredPer_db.result_pi_hip (MODELO_HIPOTECARIO_B1)
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_hip_estandar(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **pct_pi**: Probabilidad Incumplimiento
# MAGIC - **pct_pdi** : Pérdida dado el incumplimiento del cliente (PDI)
# MAGIC - **pct_pe**: Pérdida Esperada
# MAGIC - **mnt_operacion**: EAD o Total saldo IFRS
# MAGIC - **mnt_provision**: Provisión  
# MAGIC - **pct_ltv**: Prestamo versus Garantía (Loan-to-value)

# COMMAND ----------

query_ins_hip_b1 = """ insert into """+db_plat_tempX+""".tmp_modelo_hip_estandar
select anomes	
			,rut	
      ,dv
			,Numero_Operacion	
			,PI	
			,PDI
			,PE	
			,EAD												AS Monto_operacion
			,Provision										AS Provision
			,PVG 
from """+db_RiesgoCredX+""".result_pi_hip
where anomes = """+anomes+"""
"""

# COMMAND ----------

sql_safe(query_ins_hip_b1)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida Modelo Interno

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_plat_tempX+""".tmp_modelo_hip where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_modelo_hip ")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida Estandar

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_plat_tempX+""".tmp_modelo_hip_estandar  where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_modelo_hip_estandar ")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")